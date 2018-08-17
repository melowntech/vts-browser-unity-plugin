cmake_minimum_required(VERSION 3.5.1 FATAL_ERROR)
project(vts-browser-unity-plugin)

set(VTS_LIBS_TYPE MODULE CACHE STRING "Type of vts browser library forced to MODULE" FORCE)

add_subdirectory(externals/browser)

buildsys_fix_symlinks(${CMAKE_CURRENT_SOURCE_DIR}/src/Vts/Scripts)
buildsys_fix_symlinks(${CMAKE_CURRENT_SOURCE_DIR}/src/Vts/Documentation)
