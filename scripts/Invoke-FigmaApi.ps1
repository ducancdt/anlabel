param(
    [string]$FileKey = "zdN71qfzrYV6pPt1b2FRRc",
    [string]$NodeId = "2:2",
    [ValidateSet("document", "nodes", "images", "components", "styles")]
    [string]$Action = "nodes",
    [string]$Token = $env:FIGMA_PAT
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Token)) {
    throw "Figma token is required. Pass -Token or set the FIGMA_PAT environment variable."
}
$headers = @{ "X-Figma-Token" = $Token }

switch ($Action) {
    "document" {
        $uri = "https://api.figma.com/v1/files/$FileKey"
        return Invoke-RestMethod -Uri $uri -Headers $headers
    }
    "nodes" {
        $nodeParam = if ([string]::IsNullOrWhiteSpace($NodeId)) { "2:2" } else { $NodeId.Replace("-", ":") }
        $uri = "https://api.figma.com/v1/files/$FileKey/nodes?ids=$nodeParam"
        return Invoke-RestMethod -Uri $uri -Headers $headers
    }
    "images" {
        $nodeParam = if ([string]::IsNullOrWhiteSpace($NodeId)) { "2:2" } else { $NodeId.Replace("-", ":") }
        $uri = "https://api.figma.com/v1/images/$FileKey?ids=$nodeParam&format=png"
        return Invoke-RestMethod -Uri $uri -Headers $headers
    }
    "components" {
        $uri = "https://api.figma.com/v1/files/$FileKey/components"
        return Invoke-RestMethod -Uri $uri -Headers $headers
    }
    "styles" {
        $uri = "https://api.figma.com/v1/files/$FileKey/styles"
        return Invoke-RestMethod -Uri $uri -Headers $headers
    }
}
