# VTS Browser Integration Plugin For Unity 3D Game Engine

VTS 3D Geospatial Software Stack is a state-of-the-art, full-stack open source platform for 3D geospatial application development.
Consisting of 3D streaming backend components and of JavaScript and C++ frontend libraries, VTS provides for interactive 3D rendering of geospatial content on the Web, on the desktop or on mobile.

This plugin integrates the VTS browser library into Unity 3D.

## Online Documentation

Detailed Vts Unity Plugin documentation is at the
[github wiki](https://github.com/Melown/vts-browser-unity-plugin/wiki).

Complete source code for the plugin is at
[github](https://github.com/Melown/vts-browser-unity-plugin).
Use the github issues for questions, reporting problems or suggestions.

Documentation for the whole VTS is at
[Read the Docs](https://melown.readthedocs.io).

Contact us at vts-plugin@melown.com for further support (check the github issues first!).

## Demos

Try the demo scenes!
They are simple and should give you basic understanding how things works.

## Getting Started

Create new scene.
Add an Empty game object and attach Vts Map script to it.
Attach Vts Camera Cmd Bufs and Vts Navigation to the Main Camera.
Enable Atmosphere on the camera and change Clear Flags to Solid Color.
Finally, drag the previously created Empty game object (the one with Vts Map) into the Map Object property of the camera.
Hit the Play button. It is really this simple :D

## Scripts Overview

- Vts Map: attach this script to a Game Object that will represent the map (the planet).
  This component maintains the internal state of the whole VTS Browser (downloaded meshes, textures and other metadata).
  All other related components will then work through reference to this object.
  Note that this script, on its own, does not make the map visible in any view.

- Vts Camera Cmd Bufs: attach this script to Unity Camera.
  This script will add Graphics Command Buffers to the camera and use them to render the map.

- Vts Camera Objects: this is an alternative to Vts Camera Cmd Bufs.
  This script will instantiate and maintain new Game Objects that represent chunks of the map.

- Vts Collider Probe: attach this script to object around which you would like the map to be physically interactive.
  This script, much like Vts Camera Objects, instantiates new Game Objects with mesh colliders.
  Note that the meshes instantiated by the Vts Collider Probe are not visible.

- Vts Navigation: attach to a Game Object with any Vts Camera.
  This script grabs mouse input and applies it to the camera. It supports pan, rotation and zoom.
  It will change Control Transformation to Vts on the Vts Camera script in order to work properly.

- Vts Map Make Local: attach to the Game Object with Vts Map.
  This script will move/rotate the Game Object in such way that the configured latitude and longitude coordinates are at the origin of Unity world coordinates.
  This is required for the Vts Camera Objects and Vts Collider Probe scripts to overcome floating point precision issues, but will limit the playable area only to close neighborhood.
  See the online documentation for details.

- Vts Map Shifting Origin: attach to the Game Object with Vts Map.
  When the focused object moves further from world origin than a configured threshold, this script will repeat the process that Vts Map Make Local does, such that the focus object is moved back to the world origin.
  This script transforms all Game Objects that are marked with Vts Object Shifting Origin Base component to maintain their relative positions and orientations with the map.

- Vts Object Shifting Origin (Base): attach to any Game Objects, except the map.
  This component marks the Game Object to be moved by Vts Map Shifting Origin script.
  It is required on the Game Object that is selected as focus for the shifting.
  This script is also automatically attached to all objects instantiated by Vts Camera Objects and Vts Collider Probe.

## Notes

### Map Configuration

The mapconfig specifies, among other, coordinate systems and data sources.

The default map configuration uses our open example.
You may change the mapconfig url in the Vts Map component to any other, including your own instance of VTS backend.
See our VTS documentation for more information about map configuration.

### Coordinate Conversions

When the mapconfig is loaded, the Vts Map component provides methods UnityToVtsNavigation and VtsNavigationToUnity for coordinate conversions.
These methods also take into account the transformation of the Game Object itself, eg. the transformation made by Vts Map Make Local script.

### VTS as Native Plugin

The whole VTS Browser library is composed of several 3rd-party libraries and many c++ sources with complicated build rules.
This makes it unsuitable to be build directly by Unity Editor from sources.
Therefore, precompiled binaries are packaged.

Officially supported platforms:
- Windows
- Mac OS
- iOS (il2cpp) - see the online resources for further instructions
- Linux

The VTS Browser requires 64 bit architecture (on all platforms).

If your target platform/architecture is not supported, you may try to build the VTS Browser library from source code.

Please note, we provide NO support for webgl player in this plugin.
Use our javascript browser library if you want to use VTS on a website.

### Vts Cache

The VTS browser library is caching all downloaded files.
The cache is located at:
- Windows: %HOME%/.cache/vts-browser
  - eg. C://Users/name/.cache/vts-browser
- Mac: $HOME/.cache/vts-browser
  - eg. /Users/name/.cache/vts-browser
- iOS: none
  - caching is handled by ios directly
- Linux: $HOME/.cache/vts-browser
  - eg. /home/name/.cache/vts-browser

### Known Issues

- An exception 'Screen position out of view frustum' is thrown occasionally.
  It does not cause any perceptible issues.
