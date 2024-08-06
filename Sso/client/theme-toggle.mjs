'use strict'

function getStoredTheme() {
    return localStorage.getItem('theme')
}
function setStoredTheme(theme) {
    localStorage.setItem('theme', theme)
}

function getPreferredTheme() {
    const storedTheme = getStoredTheme()
    if (storedTheme) {
        return storedTheme
    }

    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function getTheme() {
    return document.documentElement.getAttribute('data-bs-theme')
}

function setTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme)
}

setTheme(getPreferredTheme())

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
    const theme = e.matches ? 'dark' : 'light'
    setStoredTheme(theme)
    setTheme(theme)
})

function registerClickHandler() {
    document.querySelectorAll('.theme-toggle')
        .forEach(toggle => {
            toggle.addEventListener('click', () => {
                const theme = getTheme() === 'dark' ? 'light' : 'dark'
                setStoredTheme(theme)
                setTheme(theme)
            })
        })
}

if (document.readyState !== 'loading') {
    registerClickHandler()
} else {
    window.addEventListener('DOMContentLoaded', registerClickHandler)
}