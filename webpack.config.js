// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");
const CopyPlugin = require("copy-webpack-plugin");

module.exports = {
    mode: "development",
    entry: "./src/App.fsproj",
    output: {
        path: path.join(__dirname, "dist"),
        filename: "bundle.js",
    },
    plugins: [
        new CopyPlugin({
          patterns: [
            { from: "public/global.css", to: "global.css" },
            { from: "public/favicon.png", to: "favicon.png" },
            //{ from: "public/gh-pages", to: "" }
          ],
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