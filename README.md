# 설치
Packages/manifest.json을 다음과 같이 수정해주세요.
```
{
  "scopedRegistries": [
    {
      "name": "Unity NuGet",
      "url": "https://unitynuget-registry.azurewebsites.net",
      "scopes": [
        "org.nuget"
      ]
    }
  ],
  "dependencies": {
    "com.sgkim6326.meshlib": "https://github.com/sgkim6326/MeshLib.git",
```