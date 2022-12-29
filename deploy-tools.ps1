function Build()
{
    Push-Location .\src
    try {
        dotnet build
    } 
    finally {
        Pop-Location
    }
}

function Publish()
{
    Push-Location .\src
    try {
        if (Test-Path "..\publish") 
        {
            Remove-Item "..\publish" -Force -Recurse
        }

        dotnet publish -o ..\publish
    } 
    finally {
        Pop-Location
    }
}

function Deploy()
{
    $targetPath = $env:DEPLOY_TARGET_PATH
    if (!$targetPath) {
        throw "Missing DEPLOY_TARGET_PATH"
    }
 
    if (!(Test-Path -Path $targetPath))
    {
        mkdir $targetPath
    }

    # Take site offline
    Copy-Item -Path ".\deploy\app_offline.htm" -Destination "$targetPath" -Force
    Start-Sleep -Seconds 2

    # Replace app
    Remove-Item -Path "$targetPath\*.*" -Exclude "*.local.json","app_offline.htm" -Recurse -Force
    Copy-Item ".\publish\*" -Exclude "*.local.json" $targetPath -Recurse -Force 

    # Take site online
    Remove-Item -Path "$targetPath\app_offline.htm"
}