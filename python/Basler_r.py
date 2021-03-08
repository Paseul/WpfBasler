'''
A simple Program for grabing video from basler camera and converting it to opencv img.
Tested on Basler acA1300-200uc (USB3, linux 64bit , python 3.5)

'''
from pypylon import pylon
import numpy as np
from matplotlib import pyplot as plt
import cv2
import argparse
from Iso11146 import Iso11146
from imutils.video import FPS

# construct the argument parse and parse the arguments
ap = argparse.ArgumentParser()
ap.add_argument("-o", "--output", default="Original.avi",
	help="path to the (optional) video file")
ap.add_argument("-e", "--exposure_time", type=int, default=20000,
	help="Set Exposure Time")
ap.add_argument("-min", "--minimum_thresh", type=int, default=50,
	help="minimum thresh value")
ap.add_argument("-max", "--maximum_thresh", type=int, default=255,
	help="maximum thresh value")
args = vars(ap.parse_args())

maxCamerasToUse = 2

# Get the transport layer factory.
tlFactory = pylon.TlFactory.GetInstance()

# Get all attached devices and exit application if no device is found.
devices = tlFactory.EnumerateDevices()
if len(devices) == 0:
    raise pylon.RuntimeException("No camera present.")

 # Create an array of instant cameras for the found devices and avoid exceeding a maximum number of devices.
cameras = pylon.InstantCameraArray(min(len(devices), maxCamerasToUse))

l = cameras.GetSize()

# Create and attach all Pylon Devices.
for i, cam in enumerate(cameras):
    cam.Attach(tlFactory.CreateDevice(devices[i]))

    # Print the model name of the camera.
    print("Using device ", cam.GetDeviceInfo().GetModelName())

# Grabing Continusely (video) with minimal delay
cameras.StartGrabbing()
converter = pylon.ImageFormatConverter()

# converting to opencv bgr format
converter.OutputPixelFormat = pylon.PixelType_BGR8packed
converter.OutputBitAlignment = pylon.OutputBitAlignment_MsbAligned

# save video
# videoWriter = cv2.VideoWriter()

# fourcc = cv2.VideoWriter_fourcc(*'XVID')
# videoWriter.open(args["output"], fourcc, 14, (1920, 1374), False)   

fps1 = FPS().start()
fps2 = FPS().start()

vs = cv2.VideoCapture("rtsp://admin:laser123@192.168.1.64:554/Streaming/Channels/101/")

while cameras.IsGrabbing():
    grabResult = cameras.RetrieveResult(5000, pylon.TimeoutHandling_ThrowException)   

    if grabResult.GrabSucceeded():
        # Access the image data
        image = converter.Convert(grabResult)
        img = image.GetArray()        
        img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        np.where(img < 5, 0, img)  

        # cv2.putText(img, fps, (10, 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)

        # videoWriter.write(img)
        # print(img.shape)
        
        cv2.namedWindow('acA2040-90umNIR', cv2.WINDOW_NORMAL)
        cv2.namedWindow('acA3800-14uc', cv2.WINDOW_NORMAL)
        if img.shape == (2048, 2048):
            fps1.update()
            fps1.stop()        
            img = cv2.resize(img, (640, 480))
            text = "{:.2f}".format(fps1.fps())
            cv2.putText(img, text, (10, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 0), 2)
            cv2.imshow('acA2040-90umNIR', img)
        
            success, frame = vs.read()
            fps2.update()
            fps2.stop()        
            img = cv2.resize(frame, (640, 480))
            text = "{:.2f}".format(fps2.fps())
            cv2.putText(img, text, (10, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)
            cv2.imshow('acA3800-14uc', img)       

        k = cv2.waitKey(1)           

        if k == 27:
            break

    grabResult.Release()   
    
# Releasing the resource    
cameras.StopGrabbing()
# videoWriter.release()
cv2.destroyAllWindows()
