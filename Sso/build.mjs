import * as esbuild from 'esbuild'
import {sassPlugin} from 'esbuild-sass-plugin'
import browserslist from "browserslist";
import {
    esbuildPluginBrowserslist,
    resolveToEsbuildTarget,
} from "esbuild-plugin-browserslist";

await esbuild.build({
    entryPoints: ['client/main.mjs'],
    bundle: true,
    minify: true,
    sourcemap: true,
    outdir: 'wwwroot/app',
    plugins: [
        esbuildPluginBrowserslist(browserslist("defaults"), {
            printUnknownTargets: false,
        }),
        sassPlugin()
    ]
})