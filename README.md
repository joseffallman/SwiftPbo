# SwiftPbo
[![Build Status](https://travis-ci.org/headswe/SwiftPbo.svg?branch=master)](https://travis-ci.org/headswe/SwiftPbo)

# Copyright
  Copyright 2015-2016, 2018 by headswe
  Copyright 2018 by dedmen
  Copyright 2019 by Josef Fällman
  
  Licensed under GNU Lesser General Public License 3.0

# How to use
You´ll find a .dll in the download folder. Download it and use it in your project.
In namespace SwiftPbo you´ll find PboArchive class. 

Use the static method create to pack a mission folder to a pbo file.
PboArchive.Create(pathToMissionFolder)

If you want to have your pbo in a specific folder, go ahed and send it in.
PboArchive.Create(pathToMissionFolder, pathAndNameToPboFile)

And if you want to filter the files in mission folder before packing it, 
create a new Config.FilterFileConfig() object and send it to the Create method.
PboArchive.Create(pathToMissionFolder, pathAndNameToPboFile, yourConfig)

Possible config filters:
	ExcludedExtensions - Strings with extensions to exclude.
	ExcludedSubstringInPath - Strings with filenames/folders to exclude.
	ExcludeAllHidden - If hidden folders/files should be excluded.