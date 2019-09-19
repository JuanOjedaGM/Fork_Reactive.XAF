![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.Reactive.Logger.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.Reactive.Logger.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/Reactive.Logger.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+Reactive.Logger) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/Reactive.Logger.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+Reactive.Logger)
# About 

The `Reactive.Logger` module monitors calls to the Reactive delegates (OnNext, OnSubscribe, OnDispose, OnCompleted, OnError). All calls are saved to the application database. For more head to the details section.

The module uses the next two strategies:
1. It monitors the `DetailView` creation and modifies its Reactive.Logger property according to model configuration. However later Reactive.Logger property modifications are allowed.
2. It monitors the `Reactive.Logger` modifiation and cancels it if the `LockReactive.Logger` attribute is used.
## Installation 
1. First you need the nuget package so issue this command to the `VS Nuget package console` 

   `Install-Package Xpand.XAF.Modules.Reactive.Logger`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.Reactive.LoggerModule));
    ```

The module is not integrated with any `eXpandFramework` module. You have to install it as described.

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).
## Dependencies
`.NetFramework: `

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
 |**DevExpress.Persistent.Base**|**Any**
 |**DevExpress.ExpressApp.ConditionalAppearance**|**Any**
 |**DevExpress.Xpo**|**Any**
|akarnokd.reactive_extensions|0.0.27-alpha
 |System.Reactive|4.1.6
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/Xpand.XAF.Modules.Reactive)|1.2.47
 |[Xpand.VersionConverter](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)|1.0.34

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call when [XafApplication.SetupComplete](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.XafApplication.SetupComplete).
```ps1
((Xpand.XAF.Modules.Reactive.LoggerModule) Application.Modules.FindModule(typeof(Xpand.XAF.Modules.Reactive.LoggerModule))).Unload();
```

## Details
The module extends the `IModelReactiveModules` to provide a list of detected TraceSources allowing to configure them further.
![image](https://user-images.githubusercontent.com/159464/64830050-63c43a00-d5d7-11e9-919d-ac5df92646af.png)


![image](https://user-images.githubusercontent.com/159464/55380067-b7f6c880-5527-11e9-96a1-053fd44095e7.png)

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/Reactive.Logger)

### Examples

The module is valuable in scenarios similar to:
1. When you want to `navigate` from a `ListView` to a `DetailView` without the intermediate view which is set to View Reactive.Logger.
2. When you develop a `master-detail` layout and you want to control the Reactive.Logger state of your

`XtraDashboardModule` ,`ExcelImporterModule` are modules that use the `Reactive.LoggerModule`.  

Next screenshot is an example from ExcelImporter from the view tha maps the Excel columns with the BO members. 

![image](https://user-images.githubusercontent.com/159464/55381194-238e6500-552b-11e9-8314-f1b1132d09f3.png)