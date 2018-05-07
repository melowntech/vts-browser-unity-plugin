cmake_minimum_required(VERSION 3.5.1 FATAL_ERROR)
project(vts-browser-unity-plugin)

set(VTS_ENABLE_CSHARP ON CACHE BOOL "generate csharp libraries" FORCE)

add_subdirectory(externals/browser)

set_target_properties(vts-browser-cs vts-renderer-cs vts-browser-minimal-cs PROPERTIES VS_GLOBAL_TargetFrameworkVersion v3.5)


