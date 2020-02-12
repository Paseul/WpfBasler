'''
A simple Program for grabing video from basler camera and converting it to opencv img.
Tested on Basler acA1300-200uc (USB3, linux 64bit , python 3.5)

'''
from pypylon import pylon
import numpy as np
import cupy as cp
from matplotlib import pyplot as plt
import cv2

# conecting to the first available camera
camera = pylon.InstantCamera(pylon.TlFactory.GetInstance().CreateFirstDevice())

# Grabing Continusely (video) with minimal delay
camera.StartGrabbing(pylon.GrabStrategy_LatestImageOnly) 
converter = pylon.ImageFormatConverter()

# converting to opencv bgr format
converter.OutputPixelFormat = pylon.PixelType_BGR8packed
converter.OutputBitAlignment = pylon.OutputBitAlignment_MsbAligned

# save video
videoWriter = cv2.VideoWriter()

fourcc = cv2.VideoWriter_fourcc(*'XVID')
videoWriter.open("Original.avi", fourcc, 14, (1920, 1374), False)   

while camera.IsGrabbing():
    grabResult = camera.RetrieveResult(5000, pylon.TimeoutHandling_ThrowException)   

    if grabResult.GrabSucceeded():
        # Access the image data
        image = converter.Convert(grabResult)
        img = image.GetArray()
        img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        np.where(img < 5, 0, img)  

        ret, thresh = cv2.threshold(img, 200, 255, 0)
        thresh = cv2.erode(thresh, None, iterations=4)
        thresh = cv2.dilate(thresh, None, iterations=4)

        contours, hierarchy = cv2.findContours(thresh,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
        for c in contours:
            # discard small contours
            if (cv2.contourArea(c) < 1000):
                continue
            
            # calculate moments
            M = cv2.moments(c)
            
            if (M["m00"]!= 0):
                cX = int(M["m10"] / M["m00"])
                cY = int(M["m01"] / M["m00"])
                cX2 = int(M["mu20"] / M["m00"])
                cXY = int(M["mu11"] / M["m00"])
                cY2 = int(M["mu02"] / M["m00"])

                dX = int((2*(2**0.5)*((cX2 + cY2) + 2*abs(cXY))**0.5).real)
                dY = int((2*(2**0.5)*((cX2 + cY2) - 2*abs(cXY))**0.5).real)

                if((cX2 - cY2)!=0):
                    t = 2 * cXY / (cX2 - cY2)
                else:
                    t = 0
                
                theta = 0.5 * np.arctan(t) * 180
                cv2.ellipse(img, (cX, cY), (dX, dY), theta, 0, 360, (255, 255, 255), 5)

        img = cv2.resize(img, (1920, 1374))    

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
