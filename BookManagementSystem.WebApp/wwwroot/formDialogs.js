window.bmsFormDialogs = {
    focusById: function (elementId) {
        if (!elementId) {
            return;
        }

        const element = document.getElementById(elementId);
        if (element) {
            requestAnimationFrame(() => element.focus({ preventScroll: false }));
        }
    },

    focusFirstInvalid: function (formId) {
        const form = document.getElementById(formId);
        if (!form) {
            return;
        }

        const invalid = form.querySelector(
            "[aria-invalid='true'], .mud-input-error input, .mud-input-error textarea, .mud-input-error [tabindex], input.invalid, select.invalid, textarea.invalid");

        if (invalid) {
            requestAnimationFrame(() => {
                invalid.focus({ preventScroll: false });
                invalid.scrollIntoView({ block: "center", behavior: "smooth" });
            });
        }
    }
};
