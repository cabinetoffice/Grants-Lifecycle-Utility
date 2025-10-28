function lockFields(executionContext) {
    var formContext = executionContext.getFormContext();
    // Iterate over all controls on the form
    formContext.ui.controls.forEach(function (control, index) {
        // Corrected variable name from 'control Name' to 'controlName'
        var controlName = control.getName();
        // Check if the control is a part of the BPF (by name convention)
        if (controlName && controlName.indexOf("header_process_") !== -1) {
            control.setDisabled(true); // Disable the control if it's part of BPF
        }
    });
}
