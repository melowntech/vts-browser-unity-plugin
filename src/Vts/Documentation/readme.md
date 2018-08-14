# VTS Browser Integration Plugin For Unity 3D Game Engine

VTS 3D Geospatial Software Stack is a state-of-the-art, full-stack open source platform for 3D geospatial application development. Consisting of 3D streaming backend components and of JavaScript and C++ frontend libraries, VTS provides for interactive 3D rendering of geospatial content on the Web, on the desktop or on mobile.

This plugin integrates the VTS browser library into Unity 3D.

## Complete Documentation

Vts Unity Plugin documentation is at the
[wiki](https://github.com/Melown/vts-browser-unity-plugin/wiki).

Complete source code for the plugin is at
[github](https://github.com/Melown/vts-browser-unity-plugin).
Use the github issues for reporting problems or suggestions.

Documentation for the whole VTS is at
[Read the Docs](https://melown.readthedocs.io).

## Demos

Try the demo scenes.
Experiment with different settings :D

## Getting Started

Create new scene.
Add an Empty game object and attach Vts Map and Vts Map Navigation scripts to it.
Attach Vts Camera Cmd Bufs to the Main Camera and switch Control Transformations, Control Near Far and Control Fov all to Vts.
Change Clear Flags to Solid Color and enable Atmosphere.
Finally drag the previously created Empty game object into the Map Object property of the camera.
Hit the Play button. It is really this simple :D

# Notes

## Known Issues

- Occasionally, an exception 'Screen position out of view frustum' is thrown. The exception does not cause any perceptible issues.

## Vts Cache

The VTS browser library is caching all downloaded files.
The cache is located at:
- Windows: %HOME%/.cache/vts-browser
  - eg. C://Users/name/.cache/vts-browser

