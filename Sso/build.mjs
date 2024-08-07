import * as esbuild from 'esbuild'
import {sassPlugin} from 'esbuild-sass-plugin'
import browserslist from "browserslist"
import { esbuildPluginBrowserslist } from 'esbuild-plugin-browserslist'
import { PurgeCSS } from 'purgecss'

import { writeFile } from 'node:fs/promises'

const purgeCSSPlugin = (options = {}) => {
    return {
        name: 'purgecss',
        setup(build) {
            build.onEnd(async (result) => {
                const cssOutputs = Object.keys(result.metafile.outputs).filter(k => k.endsWith('.css'))
                for (const outputFile of cssOutputs) {
                    const results = await new PurgeCSS().purge({
                        ...options,
                        css: [outputFile]
                    })

                    for (let result of results) {
                        await writeFile(result.file, result.css)
                    }
                }
            })
        },
    }
}

await esbuild.build({
    metafile: true,
    entryPoints: ['client/main.mjs'],
    bundle: true,
    minify: true,
    sourcemap: false,
    outdir: 'wwwroot/app',
    plugins: [
        esbuildPluginBrowserslist(browserslist("defaults"), {
            printUnknownTargets: false,
        }),
        sassPlugin(),
        purgeCSSPlugin({
            content: ['./Pages/**/*.cshtml', "wwwroot/app/**/*.js"]
        }),
    ]
})