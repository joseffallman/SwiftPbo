# How to use
You´ll find a .dll in the download folder. Download it and use it in your project.<br>
In namespace SwiftPbo you´ll find PboArchive class. 

Use the static method create to pack a mission folder to a pbo file.<br>
<b>PboArchive.Create(pathToMissionFolder)</b>

If you want to have your pbo in a specific folder, go ahed and send it in.<br>
<b>PboArchive.Create(pathToMissionFolder, pathAndNameToPboFile)</b>

And if you want to filter the files in mission folder before packing it, 
create a new Config.FilterFileConfig() object and send it to the Create method.<br>
<b>PboArchive.Create(pathToMissionFolder, pathAndNameToPboFile, yourConfig)</b>

Possible config filters:<ol>
	<li>ExcludedExtensions - Strings with extensions to exclude.</li>
	<li>ExcludedSubstringInPath - Strings with filenames/folders to exclude.</li>
	<li>ExcludeAllHidden - If hidden folders/files should be excluded.</li></ol>
	
# Copyright
  Copyright 2015-2016, 2018 by headswe<br>
  Copyright 2018 by dedmen<br>
  Copyright 2019 by Josef Fällman<br>
  
  Licensed under GNU Lesser General Public License 3.0

