'''
A simple Program for grabing video from basler camera and converting it to opencv img.
Tested on Basler acA1300-200uc (USB3, linux 64bit , python 3.5)

'''
from pypylon import pylon
import numpy as np
from matplotlib import pyplot as plt
import cv2
import argparse

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

# conecting to the first available camera
camera = pylon.InstantCamera(pylon.TlFactory.GetInstance().CreateFirstDevice())

# Grabing Continusely (video) with minimal delay
camera.StartGrabbing(pylon.GrabStrategy_LatestImageOnly) 
converter = pylon.ImageFormatConverter()

# Control Exposure Time
camera.ExposureTime.SetValue(args["exposure_time"])

# converting to opencv bgr format
converter.OutputPixelFormat = pylon.PixelType_BGR8packed
converter.OutputBitAlignment = pylon.OutputBitAlignment_MsbAligned

# save video
videoWriter = cv2.VideoWriter()

fourcc = cv2.VideoWriter_fourcc(*'XVID')
videoWriter.open("test_{}us.avi".format(args["exposure_time"]), fourcc, 90, (2048, 2048), False)   

while camera.IsGrabbing():
    grabResult = camera.RetrieveResult(5000, pylon.TimeoutHandling_ThrowException)   

    if grabResult.GrabSucceeded():
        # Access the image data
        image = converter.Convert(grabResult)
        img = image.GetArray()
        img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        videoWriter.write(img)              

        cv2.namedWindow('title', cv2.WINDOW_NORMAL)
        cv2.imshow('title', img)

        k = cv2.waitKey(1)           

        if k == 27:
            break

    grabResult.Release()   
    
# Releasing the resource    
camera.StopGrabbing()
videoWriter.release()
cv2.destroyAllWindows()
