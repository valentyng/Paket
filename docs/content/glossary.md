# Glossary

### paket.dependencies

The [`paket.dependencies` file](dependencies-file.html) is used to specify rules regarding your application's dependencies.

### paket.lock

The [`paket.lock` file](lock-file.html) records the concrete dependency resolution of all direct and indirect dependencies of your project.

### paket.references

The [`paket.references` files](references-files.html) are used to specify which dependencies are to be installed into the MSBuild projects in your repository.

### paket.template

Thee [`paket.template` files](template-files.html) are used to specify rules to create new NuGet packages by using the [`paket pack` command](paket-pack.html).

### .paket folder

This folder is used the same way a `.nuget` folder is used for the NuGet package restore. Place this folder into the root of your solution. 
It should include the paket.targets and [paket.bootstrapper.exe](https://github.com/fsprojects/Paket/releases/latest) files which can be downloaded from GitHub. 
The bootstrapper.exe will always download the latest version of the paket.exe file into this folder.