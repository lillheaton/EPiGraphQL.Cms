import updateFile from './updateFile'

export default function setVersion(version) {
  console.log('asklnfaslkgnlaksn')
  return updateFile('Updating package.json version', './package.json', data =>
    data.replace(/"version": "(.*)"/, `"version": "${version}"`)
  )
    .then(
      updateFile('Updating appveyor.yml version', './appveyor.yml', data =>
        data.replace(/version: (.*)\./, `version: ${version}.`)
      )
    )
    .then(
      updateFile(
        'Updating Eols.EPiGraphQL.Core.csproj version',
        './Eols.EPiGraphQL.Core/Eols.EPiGraphQL.Core.csproj',
        data =>
          data.replace(
            /<VersionPrefix>(.*)<\/VersionPrefix>/,
            `<VersionPrefix>${version}</VersionPrefix>`
          )
      )
    )
}
