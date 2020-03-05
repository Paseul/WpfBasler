# WpfBasler

## Introduction
This project used OpenCV with the implementation of the Basler camera in WPF format.<br>
Reference was made to the [Basler_WPF](https://github.com/cesar-vargas88/Basler_WPF) project using Emgu.CV to import WPF style.<br>
It will additionally implement OpenCV functions that can utilize camera images.

|Title|Content|
|:--:|:--:|
|Platform|Windows10(x64)|
|Development tool|Visual Studio 2019|
|Framework|.Net Framework 4.7.2|
|Target device| acA3800-14uc|

## Getting Started
1. Clone Github in your computer
2. Install following Nuget Packages
  - OpenCvSharp4 (used version: v4.1.1.20191216)
  - OpenCvSharp4.runtime.win (used version: v4.1.1.20191216)
  - OpenCvSharp4.Windows(used version: v4.1.1.20191216)
3. Install [pylon 6.0.1 Camera Software Suite Windows](https://www.baslerweb.com/en/sales-support/downloads/software-downloads/pylon-6-0-1-windows/)
  - Add reference path: C:\Program Files\Basler\pylon 6\Development\include\
4. Connect to Basler Camera to your computer and compile the project.
