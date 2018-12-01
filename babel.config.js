/* eslint-env node */

module.exports = function(api) {
  api.cache(true)

  return {
    presets: [
      // [
      //   '@babel/preset-env',
      //   {
      //     modules: false,
      //     useBuiltIns: 'entry',
      //     targets: {
      //       browsers: ['> 2%', 'IE 11']
      //     }
      //   }
      // ]
      '@babel/env'
      // '@babel/preset-env'
    ],

    plugins: [
      // '@babel/plugin-proposal-class-properties',
      '@babel/plugin-proposal-object-rest-spread'
      // '@babel/syntax-dynamic-import'
    ]
  }
}
