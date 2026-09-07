using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    private const string ProtectedFolderName = "GLU - Document Library";

    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Only apply custom delete protection to this operation.
        if (!string.Equals(
                this.Context.OperationId,
                "DeleteFileOrFolder",
                StringComparison.OrdinalIgnoreCase))
        {
            return await this.Context.SendAsync(
                this.Context.Request,
                this.CancellationToken
            );
        }

        var request = this.Context.Request;
        var requestUri = request.RequestUri;

        if (requestUri == null)
        {
            return CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Deletion rejected because the request URI could not be determined."
            );
        }

        // Extract fileId from:
        // /drive/v3/files/{fileId}
        var segments = requestUri.AbsolutePath
            .TrimEnd('/')
            .Split('/');

        if (segments.Length == 0)
        {
            return CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Deletion rejected because the Google Drive file ID could not be determined."
            );
        }

        var fileId = Uri.UnescapeDataString(
            segments[segments.Length - 1]
        );

        // Hard block Google's root alias.
        if (string.Equals(
                fileId,
                "root",
                StringComparison.OrdinalIgnoreCase))
        {
            return CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Deletion rejected. The Google Drive root folder cannot be deleted."
            );
        }

        /*
         * Retrieve the target's metadata before deleting it.
         *
         * Example:
         * GET /drive/v3/files/{fileId}
         *     ?fields=id,name,mimeType,parents
         *     &supportsAllDrives=true
         */

        var metadataUri =
            $"{requestUri.Scheme}://{requestUri.Host}" +
            $"/drive/v3/files/{Uri.EscapeDataString(fileId)}" +
            "?fields=id,name,mimeType,parents&supportsAllDrives=true";

        using (var metadataRequest = new HttpRequestMessage(
            HttpMethod.Get,
            metadataUri))
        {
            var metadataResponse = await this.Context.SendAsync(
                metadataRequest,
                this.CancellationToken
            );

            if (!metadataResponse.IsSuccessStatusCode)
            {
                return metadataResponse;
            }

            var metadataJson =
                await metadataResponse.Content.ReadAsStringAsync();

            var metadata =
                JObject.Parse(metadataJson);

            var name =
                metadata["name"]?.ToString() ?? string.Empty;

            var mimeType =
                metadata["mimeType"]?.ToString() ?? string.Empty;

            /*
             * Safety rule:
             *
             * Refuse to delete anything whose name contains
             * "GLU - Document Library".
             *
             * Examples blocked:
             *
             * GLU - Document Library
             * GLU - Document Library (DEV)
             * GLU - Document Library (UAT)
             * GLU - Document Library (PROD)
             */

            if (name.IndexOf(
                    ProtectedFolderName,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    $"Deletion rejected. Protected GLU folder '{name}' cannot be deleted."
                );
            }

            /*
             * Optional additional protection:
             * make this endpoint folder-only.
             *
             * This prevents someone accidentally using the action
             * to permanently delete an individual file.
             */
            if (!string.Equals(
                    mimeType,
                    "application/vnd.google-apps.folder",
                    StringComparison.OrdinalIgnoreCase))
            {
                return CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    $"Deletion rejected. '{name}' is not a Google Drive folder."
                );
            }
        }

        /*
         * Target passed validation.
         * Execute the original DELETE request.
         */
        return await this.Context.SendAsync(
            request,
            this.CancellationToken
        );
    }

    private HttpResponseMessage CreateErrorResponse(
        HttpStatusCode statusCode,
        string message)
    {
        var response = new HttpResponseMessage(statusCode);

        response.Content = new StringContent(
            JsonConvert.SerializeObject(
                new
                {
                    error = new
                    {
                        message = message
                    }
                }
            ),
            Encoding.UTF8,
            "application/json"
        );

        return response;
    }
}