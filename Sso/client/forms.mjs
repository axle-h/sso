'use strict'

function registerPasswordToggles() {
    document.querySelectorAll('.password-input .password-toggle')
        .forEach(button => {
            const group = button.closest('.password-input')
            const input = group?.querySelector('input')
            if (!input) {
                return
            }

            button.addEventListener('click', () => {
                const wasVisible = input.type === 'text'
                input.type = wasVisible ? 'password' : 'text'
                group.classList.toggle('is-visible', !wasVisible)
                button.setAttribute('aria-label', wasVisible ? 'Show password' : 'Hide password')
                input.focus()
            })
        })
}

// browsers only fire submit once native validation passes, so this cannot
// disable the button on a form that never actually submits
function registerSubmitStates() {
    document.querySelectorAll('form.busy-on-submit')
        .forEach(form => {
            form.addEventListener('submit', () => {
                form.querySelectorAll('button[type=submit]').forEach(button => {
                    button.disabled = true
                    button.querySelector('.spinner-border')?.classList.remove('d-none')
                })
            })
        })
}

function register() {
    registerPasswordToggles()
    registerSubmitStates()
}

if (document.readyState !== 'loading') {
    register()
} else {
    window.addEventListener('DOMContentLoaded', register)
}
