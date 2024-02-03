# Table of Content
- [Table of Content](#table-of-content)
- [Introduction and Prerequisites](#introduction-and-prerequisites)
- [Basic Setup](#basic-setup)
  - [Clone the repositories](#clone-the-repositories)
  - [Test run the setup](#test-run-the-setup)
- [Understanding the Repository Structure for TOR\_Core](#understanding-the-repository-structure-for-tor_core)
- [References](#references)

# Introduction and Prerequisites
This tutorial assumes that you have the following for development:
* [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) (aka version 17.x)
* Mount and Blades 2: Bannerlord, at least version 1.2 (as of Oct. 17, 2023, it's still in beta)
* [Git](https://git-scm.com/downloads) source control
  * Optional: GUI tool for Git (like [sourcetree](https://www.sourcetreeapp.com/) or [gitkraken](https://www.gitkraken.com/)

Some minimal skill in the following will be required:
* Experience coding in Visual Studio, specifically C#
* Know how to and love to play Bannelord!

# Basic Setup

## Clone the repositories
**!!Download the repos into the {BannerlordRootDirectory}/Modules directory!!**

The following repos will be required to run the local development environment:
* [TOR Core](https://github.com/TheOldRealms/TOR_Core)
* [TOR Armory](https://github.com/TheOldRealms/TOR_Armory)
* [TOR Environment](https://github.com/TheOldRealms/TOR_Environment)

For further information on cloning a repository in GitHub, this tutorial will help: [Cloning a repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository)

Once the repos are cloned, switch the branch to "development" for the latest working development branch.

## Test run the setup
In Visual Studio, run TOR_Core/CSharpSourceCode/TOR_Core.sln in debug mode and ensure that the game loads with TOR mod fully working as if it were installed from official channels.

# Understanding the Repository Structure for TOR_Core
The structure of the project is as follows:
* CSharpSourceCode - The Visual Studio solution for developing the module's C# code
* GUI - XML markup defining UI elements (brushes, layouts, etc.) and how they are laid out in-game
* InkStories - The Ink Story markup file for content creators (this needs C# code in the CSharpSourceCode directory to back the functionalities)
* ModuleData - XML data for Bannerlord to run the mod while debugging, also what's released to the public when the time comes
* Prefabs - Game entity hierarchy definition
* bin - Compiled code for Bannerlord to run the mod, also what's released to the public when the time comes
* music - Music definitions
* SubModule.xml - Bannerlord module metadata
* .git*, *.md - Git files and other documentation

# References
* [Taleworlds official Bannerlord modding documentation](https://moddocs.bannerlord.com/)