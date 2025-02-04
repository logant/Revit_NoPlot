# Revit No Plot

## Description
A plugin for Revit that adds no plot functionality to the print method. It can turn off subcategories with a specified identifier (default is "NPLT") or hide individual elements in the views being printed that have the identifier in the type name.
This project has been simplified somewhat for the latest release for Revit 2025. The reliance on other libraries I've developed (RevitCommon and others) has been removed, with any necessary functionality brought accross. During this process some minor functionality was lost.

Removed Functions:
- With RevitCommon removed, the connection to the RevitCommon.config file was also axed. In order to change the tab name, panel name, or help file location you would need to make the edits to the code for those three values and compile it as such.

## Dependencies
~~Uses [RevitCommon](https://github.com/logant/RevitCommon) for Revit UI integration (adding buttons to launch the commands).~~

No dependancies (besides the Revit API) are required. Everything it was leveraging from other libraries have been merged in as needed here with a slight loss of functionality.

## Known Issues:
- In Revit 2025 there is an issue with the Export PDF functionality. The plugin has been using the PrintManager to access the views and sheets being printed, but the PrintManager is now `null` during the export event. Printing using a PDF driver seems to work fine still. [Reference](https://forums.autodesk.com/t5/revit-api-forum/exception-when-accessing-printmanager-via-fileexporting-event/m-p/13161035/highlight/true#M82658)
