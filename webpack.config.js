// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");
const CopyPlugin = require("copy-webpack-plugin");

module.exports = (env, ...opt) =>
{
    console.log("env", env, opt)
    env = env || {};
    const isProd = env === "prod" || Object.keys(env).filter(k => k === "prod" && env[k]).length > 0
    const ghPages = env === "ghPages" || Object.keys(env).filter(k => k === "ghPages" && env[k]).length > 0
    console.log("ghPages", ghPages)
    console.log("isProd", isProd)
    return {
        mode: env?.production ? "production" : "development",
        entry: "./src/App.fsproj",
        output: {
            path: path.join(__dirname, "dist"),
            filename: "bundle.js",
        },
        plugins: [
            new CopyPlugin({
                patterns: [
                    {from: "public/global.css", to: "global.css"},
                    {from: "public/favicon.png", to: "favicon.png"},
                    ghPages ? {from: "public/gh-pages", to: ""} : undefined
                ].filter(r => r),
            }),
        ],
        devtool: 'source-map',
        devServer: {
            publicPath: "/",
            contentBase: "./public",
            port: 8080,
        },
        module: {
            rules: [{
                test: /\.fs(x|proj)?$/,
                use: "fable-loader"
            }]
        }
    }
}