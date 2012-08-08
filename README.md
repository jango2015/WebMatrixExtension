WebMatrixExtension
==================

##The Umbraco Extension for WebMatrix 2
This extension is designed for use with Umbraco 4.7.2 and later (note:  the extension will not work with the retired Umbraco 5).  It allows simple template editing from within WebMatrix by exposing page properties and macros (including parameters) to the WebMatrix editor.  You can also use the snippets option to insert inline Umbraco Razor macros in your Umbraco templates.

##How to use
The extension adds a tab to the WebMatrix ribbon when you are in the Files edit mode.  Navigate to your site's templates folder, which is usually **/masterpages/**.  From there, you can insert any of the page properties or macros you have defined in your Umbraco site by placing the cursor where you would like to insert the code, then selecting the item from the drop-down buttons on the **Umbraco Tools** tab from the ribbon.

## Known Issues
* Currently the extension is known to work with MS SqlServer and SQLCE, it has not been tested with SqlAzure and is not designed to work with MySQL.  
+ In some installations (where you have multiple sites under a single root) WebMatrix may not be able to determine the location of your SQLCE database and you may see a "Bad Connection String" error.
