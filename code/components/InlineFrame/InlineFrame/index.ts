import { IInputs, IOutputs } from "./generated/ManifestTypes";

export class InlineFrame implements ComponentFramework.StandardControl<IInputs, IOutputs> {

    private container: HTMLDivElement;
    private element: HTMLIFrameElement;

    constructor() {
        // Do nothing.
    }

    public init(context: ComponentFramework.Context<IInputs>, notifyOutputChanged: () => void, state: ComponentFramework.Dictionary, container: HTMLDivElement): void {
        context.mode.trackContainerResize(true);
        
        this.container = container;
        this.element = document.createElement("iframe");

        this.element.width = context.mode.allocatedWidth.toString();
        this.element.height = context.mode.allocatedHeight.toString();

        this.element.style.boxSizing = "border-box";

        if (context.parameters.URL.raw) {
            this.element.src = context.parameters.URL.raw;
            if (this.container.contains(this.element) == false) {
                this.container.append(this.element);
            }
        }
        else { 
            if (this.container.contains(this.element) == true) {
                this.container.removeChild(this.element);
            }
        }
    }

    public updateView(context: ComponentFramework.Context<IInputs>): void {
        this.element.width = context.mode.allocatedWidth.toString();
        this.element.height = context.mode.allocatedHeight.toString();

        if (context.parameters.URL.raw) { 
            this.element.src = context.parameters.URL.raw;
            if (this.container.contains(this.element) == false) {
                this.container.append(this.element);
            }
        }
        else { 
            if (this.container.contains(this.element) == true) {
                this.container.removeChild(this.element);
            }
        }
    }

    public getOutputs(): IOutputs {
        return {};
    }

    public destroy(): void {
        // Do nothing.
    }
}
