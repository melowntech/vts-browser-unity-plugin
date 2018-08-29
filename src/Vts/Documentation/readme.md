# VTS Browser Integration Plugin For Unity 3D Game Engine

VTS 3D Geospatial Software Stack is a state-of-the-art, full-stack open source platform for 3D geospatial application development.
Consisting of 3D streaming backend components and of JavaScript and C++ frontend libraries, VTS provides for interactive 3D rendering of geospatial content on the Web, on the desktop or on mobile.

This plugin integrates the VTS browser library into Unity 3D.

## Online Documentation

Detailed Vts Unity Plugin documentation is at the
[github wiki](https://github.com/Melown/vts-browser-unity-plugin/wiki).

Complete source code for the plugin is at
[github](https://github.com/Melown/vts-browser-unity-plugin).
Use the github issues for reporting problems or suggestions.

Documentation for the whole VTS is at
[Read the Docs](https://melown.readthedocs.io).

Contact us at vts-plugin@melown.com for further support.

## Demos

Try the demo scenes.
Experiment with different settings :D

## Getting Started

Create new scene.
Add an Empty game object and attach Vts Map and Vts Map Navigation scripts to it.
Attach Vts Camera Cmd Bufs to the Main Camera and switch Control Transformations, Control Near Far and Control Fov all to Vts and enable Atmosphere.
Change Clear Flags to Solid Color.
Finally, drag the previously created Empty game object (the one with Vts Map) into the Map Object property of the camera.
Hit the Play button. It is really this simple :D

## Scripts Overview

- Vts Map: attach this script to a Game Object that will represent the map (the planet).
  This components maintains the internal state of the whole VTS Browser (downloaded meshes, textures and other metadata).
  All other related components will then work through reference to this object.
  Note that this script on its own does not make the map visible in any view.

- Vts Camera Cmd Bufs: attach this script to Unity Camera.
  This script will add Graphics Command Buffers to the camera and use them to render the map.

- Vts Camera Objects: this is an alternative to Vts Camera Cmd Bufs.
  This script will instantiate and maintain new Game Objects that represent chunks of the map.
  This is useful eg. to cast shadows.
  However, the objects may suffer from precision issues.
  See the online documentation for extensive explanation.

- Vts Collider Probe: attach this script to object around which you would like the map to be physically interactive.
  This script, much like Vts Camera Objects, instantiates new Game Objects with mesh colliders.

- Vts Map Navigation: attach to the Game Object with Vts Map.
  This script grabs mouse input and applies it to the internal camera in the Vts Map.
  The internal camera can be used by changing property Control Transformation to VTS on any Vts Camera script.

- Vts Map Make Local: attach to the Game Object with Vts Map.
  This script will transform the Game Object in such way that the configured latitude and longitude coordinates are at the origin of Unity world coordinates.
  This is needed for the Vts Camera Objects and the Vts Collider Probe scripts to overcome floating point precision issues.
  See the online documentation for details.

## Notes

### VTS as Native Plugin

The whole VTS Browser library is composed of several 3rd-party libraries and many c++ sources with complicated build rules.
This makes it unsuitable to be directly build by Unity Editor from sources.
Therefore, precompiled binaries are packaged.

Officially supported platforms:
- Windows
- Mac OS
- iOS (il2cpp) - see the online resources for further instructions
- Linux

The VTS Browser requires 64 bit architecture (on all platforms).

If your target platform/architecture is not supported, you may try to build the VTS Browser library from source code.

Please note, that we provide NO support for webgl player in this plugin.
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
