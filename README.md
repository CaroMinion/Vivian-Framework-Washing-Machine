# Vivian ARCore Test Project

This repository contains a complete Unity project with the necessary modules for building a Vivian based application using [AR Foundation](https://unity.com/de/unity/features/arfoundation), i.e., in principle running on Android and iOS.

The necessary modules for running this project are contained within [Git Submodules](https://git-scm.com/book/en/v2/Git-Tools-Submodules). In order to get the project running, you will need to initialize the submodules (i.e. clone them within the project) as they are not cloned automatically upon cloning the main project.

## Initialize the Project

1. Clone the project and initialize the submodules.
+ In a GUI client, this can be proposed to you when cloning the project.
+ If you use Git through the CLI, run this command to have the submodules directly checked out:
`git clone --recurse-submodules https://gitlab.com/usability-engineering-ar-mr-vr/vivian-framework/vivian-arfoundation-test-project.git`

2. Download the matching version of Unity used for this project: **2020.3.6f1**. This can be done through Unity Hub.
+ Make sure to install the **Android build support** modules, including **OpenJDK**.

3. Add the cloned project into Unity Hub. Just navigate to the root folder and click **Select Folder**.

4. In Unity, go to *File > Build Settings...*, select **Android** in the platforms list, and click **Switch Platform**.

5. The different prototypes are available in only one scene and actually configured at runtime. This scene contains a main menu that can run the different prototypes. Which prototypes are available is configured in the `AppController` object under "Prototype Urls"

6. For understanding how prototypes are made, built, and used in the scenes, see the [Vivian Framework](#vivian-framework) section.

8. Build an Android application:
+ Without a phone connected: 'File > Build Settings... > Build' will build an APK that can be installed.
+ With a phone connected (make sure that USB debugging is enabled): 'File > Build Settings... > Build and Run' will directly install the APK on the phone and run the application.

## Vivian Framework

##### VivianFramework
This prefab is loaded at runtime. It contains the **Virtual Prototype** script that takes two parameters: 
+ **Bundle URL**: local or remote URL that points to the prototype. A local URL can point to an [AssetBundle](#asset-bundles), a folder from the **Vivian Example Prototypes/Resources** package or and other folder in a **Resources** folder. Valid examples (depending on selected URL, the prefab must be inside the folder or the asset bundle denoted by the URL):
  + AssetBundles/Android/coffeemachine
  + Microwave
+ **Prototype Prefab Name**: name of the prefab created after the prototype model. Prototypes usually contain two declinations of prefabs: one based on a blender source file (`.blend`), one based on the `.fbx` model file system. Valid examples:
  + microwave
  + coffeemachine2

 ##### ObjectPositioner
 This Game Object contains the **Object Positioner** script which is used to place the **Virtual Prototype** in the 3D space. After planes in the real world are detected, this prefab allows for positioning the selected virtual prototype, moving and rotating it, and finally confirming its position. Afterwards, the virtual prototype is started up and put into regular interactivity.
 
### Asset Bundles
Asset Bundles are archives generated by Unity. They are used in the Vivian Framework to contain the virtual prototypes' models, materials, functional specification, or any resources that are included in their *Example Prototype/Resources* folder (see the **Vivian Example Prototypes** submodule).

The creation of Asset Bundles is only required if you reference an Asset Bundle in the VivianFramework prefab. In case you reference a prototype directly from within the Resources folder in the **Vivian Example Prototypes** package, then the creation is not required.

To generate teh Asset Bundles, click *Assets > Build Vivian Prototype Bundles*. This creates an **AssetBundles** folder in the **Assets** folder, if it doesn't already exist. Another folder is created inside, depending on the build target, in our case, **Android**. The AssetBundles are contained there.
