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
        'Updating Graphify.EPiServer.Core.csproj version',
        './Graphify.EPiServer.Core/Graphify.EPiServer.Core.csproj',
        data =>
          data.replace(
            /<VersionPrefix>(.*)<\/VersionPrefix>/,
            `<VersionPrefix>${version}</VersionPrefix>`
          )
      )
    )
}
