function register() {
    document.querySelectorAll('[data-bs-toggle=modal][data-bs-target][data-delete-username]')
        .forEach(button => {
            const username = button.dataset.deleteUsername
            const modalId = button.dataset.bsTarget
            if (username && modalId) {
                button.addEventListener('click', () => {
                    document.querySelectorAll(`${modalId} .delete-user-name`).forEach(e => {
                        e.innerText = username
                    })
                    document.querySelectorAll(`input[name="Model.Username"]`).forEach(e => {
                        e.setAttribute("value", username)
                    })
                })
            }
        })
}

if (document.readyState !== 'loading') {
    register()
} else {
    window.addEventListener('DOMContentLoaded', register)
}