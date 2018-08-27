cmake_minimum_required(VERSION 3.5.1 FATAL_ERROR)
project(vts-browser-unity-plugin)

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

#if(APPLE)
#    set_target_properties(vts-browser PROPERTIES XCODE_ATTRIBUTE_ENABLE_BITCODE "NO")
#endif()

buildsys_fix_symlinks(${CMAKE_CURRENT_SOURCE_DIR}/src/Vts/Scripts)
buildsys_fix_symlinks(${CMAKE_CURRENT_SOURCE_DIR}/src/Vts/Documentation)
