# VTS Browser Integration Plugin For Unity 3D Game Engine

[VTS Browser CPP](https://github.com/melown/vts-browser-cpp) is a collection of libraries that bring VTS client capabilities to your native applications.

[This Unity Plugin](https://github.com/Melown/vts-browser-unity-plugin) integrates the VTS Browser into the popular Unity 3D game engine.

[Asset Store](https://assetstore.unity.com/packages/tools/terrain/vts-landscape-streaming-plugin-125885) prebuild version of the plugin available on Unity Asset Store.

## Example Screenshots

<img src="screenshots/hillerod-parking.png" width="430" title="Car"><img src="screenshots/earth.png" width="430" title="Earth"><img src="screenshots/alps-aircraft.png" width="860" title="Aircraft"><img src="screenshots/mercury.png" width="430" title="Mercury"><img src="screenshots/hillerod-castle.png" width="430" title="Car"><img src="screenshots/imst.png" width="430" title="Imst"><img src="screenshots/karlstejn-searching.png" width="430" title="Searching">

## Features

- The plugin handles data streaming and resource management
- Rendering is done in Unity (with custom shaders)
  - This allows you to customize the rendering process
  - Provided shaders:
    - Unlit
    - Unlit with received shadows
    - Surface shader
    - All shaders with optional custom atmosphere that works with whole-planet views
- Supports multiple cameras
- Support for physical collisions
- Real-world coordinate transformations
- Shifting origin

## Documentation

The Unity Plugin documentation is at the
[wiki](https://github.com/Melown/vts-browser-unity-plugin/wiki).

Browser documentation is available at its own
[wiki](https://github.com/melown/vts-browser-cpp/wiki).

Documentation for the whole VTS is at
[VTS Geospatial](https://vts-geospatial.org).

## Building the Plugin from Source Code

Build instructions are the same as for the [VTS Browser Build Wrapper](https://github.com/Melown/vts-browser-cpp-build-wrapper/blob/master/README.md).
Just start in the root folder of this repository to ensure that the settings from CMakeLists.txt here are applied too.

## Using the Plugin in Unity

Unity 2018 or newer is required.

### On Windows

 - Symlink \<Unity Project\>/Assets/Vts -\> \<This Repository\>/src/Vts
 - Symlink \<Unity Project\>/Assets/Vts/Plugins/Windows/vts-browser.dll -\> \<This Repository\>/build/result/\<build-type\>/vts-browser.dll
 - Configure the vts-browser.dll to be used in Editor and Standalone, x86_64, Windows

### On UWP (Universal Windows Platform)

 - beware that support for UWP is experimental
 - Symlink \<Unity Project\>/Assets/Vts -\> \<This Repository\>/src/Vts
 - Symlink \<Unity Project\>/Assets/Vts/Plugins/Uwp/vts-browser.dll -\> \<This Repository\>/build-uwp/result/\<build-type\>/vts-browser.dll
 - Configure the vts-browser.dll to be used in WSAPlayer, UWP, x64, il2cpp
 - After you make the build in Unity, open the Visual Studio project:
   - add internetClient capability to the manifest file

### On Mac

 - Symlink \<Unity Project\>/Assets/Vts -\> \<This Repository\>/src/Vts
 - Symlink \<Unity Project\>/Assets/Vts/Plugins/Mac/vts-browser.bundle -\> \<This Repository\>/build/result/\<build-type\>/vts-browser.bundle
 - Configure the vts-browser.bundle to be used in Editor and Standalone, x86_64, OSX

### For iOS

 - Symlink \<Unity Project\>/Assets/Vts -\> \<This Repository\>/src/Vts
 - Symlink \<Unity Project\>/Assets/Vts/Plugins/Ios/vts-browser.framework -\> \<This Repository\>/build-ios/result/\<build-type\>/vts-browser.framework
 - Configure the vts-browser.framework to be used in iOS
 - After you make the build in Unity, open the XCode project:
   - in the project, Build Settings, Linking, set _Runpath Search Paths_ to _@executable_path_ and _@executable_path/Frameworks_
   - in Build Phases, Copy Files, add vts-browser.framework to Destination Frameworks
     - make sure that _Code Sign On Copy_ is on

### For Linux

 - Build the vts-browser library on linux and copy it to \<Unity Project\>/Assets/Vts/Plugins/Linux/libvts-browser.so
 - Configure the libvts-browser.so to be used in Standalone, x86_64, Linux

## Bug Reports

For bug reports or enhancement suggestions use the
[Issue tracker](https://github.com/melown/vts-browser-unity-plugin/issues).

## How To Contribute

Check the [CONTRIBUTING.md](https://github.com/Melown/vts-browser-cpp/blob/master/CONTRIBUTING.md) on the VTS Browser CPP repository.
It applies equally here.

## License

See the [LICENSE](LICENSE) file.


