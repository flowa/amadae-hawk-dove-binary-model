// Webpack config for compiling tests
const path = require("path");

module.exports = {
    mode: "development",
    entry: "./test/TestRunner.fs",
    output: {
        path: path.join(__dirname, "output"),
        filename: "tests.js",
    },
    target: 'node',
    devtool: 'source-map',
    module: {
        rules: [{
            test: /\.fs(x|proj)?$/,
            use: {
                loader: "fable-loader",
                options: {
                    babel: {
                        sourceType: "module"
                    }
                }
            }
        }]
    }
}
