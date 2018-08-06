cmake_minimum_required(VERSION 3.5.1 FATAL_ERROR)
project(vts-browser-unity-plugin)

add_subdirectory(externals/browser)

buildsys_fix_symlinks(${CMAKE_CURRENT_SOURCE_DIR}/src/vts)

