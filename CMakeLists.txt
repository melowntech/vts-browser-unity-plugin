cmake_minimum_required(VERSION 3.5.1 FATAL_ERROR)
project(vts-browser-unity-plugin)

if(BUILDSYS_IOS)
    set(VTS_BROWSER_TYPE FRAMEWORK CACHE STRING "VTS_BROWSER_TYPE forced for use by Unity" FORCE)
elseif(APPLE)
    set(VTS_BROWSER_TYPE MODULE CACHE STRING "VTS_BROWSER_TYPE forced for use by Unity" FORCE)
endif()

# all final product files should be placed in ONE location
# because fu*king xcode is screwing things up
foreach(conf IN ITEMS ${CMAKE_CONFIGURATION_TYPES} ${CMAKE_BUILD_TYPE})
    string(TOUPPER ${conf} conf_upper)
    string(TOLOWER ${conf} conf_lower)
    set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${conf_upper} "${CMAKE_BINARY_DIR}/result/${conf_lower}")
    set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${conf_upper} "${CMAKE_BINARY_DIR}/result/${conf_lower}")
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${conf_upper} "${CMAKE_BINARY_DIR}/result/${conf_lower}")
endforeach(conf)

add_subdirectory(externals/browser)

buildsys_fix_symlinks(${CMAKE_CURRENT_SOURCE_DIR}/src/Vts/Scripts)
buildsys_fix_symlinks(${CMAKE_CURRENT_SOURCE_DIR}/src/Vts/Documentation)
