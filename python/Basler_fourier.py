'''
A simple Program for grabing video from basler camera and converting it to opencv img.
Tested on Basler acA1300-200uc (USB3, linux 64bit , python 3.5)

'''
from pypylon import pylon
import numpy as np
import cv2

# save video
videoWriter = cv2.VideoWriter()

# conecting to the first available camera
camera = pylon.InstantCamera(pylon.TlFactory.GetInstance().CreateFirstDevice())

# Grabing Continusely (video) with minimal delay
camera.StartGrabbing(pylon.GrabStrategy_LatestImageOnly) 
converter = pylon.ImageFormatConverter()

# converting to opencv bgr format
converter.OutputPixelFormat = pylon.PixelType_BGR8packed
converter.OutputBitAlignment = pylon.OutputBitAlignment_MsbAligned

fourcc = cv2.VideoWriter_fourcc(*'XVID')
videoWriter.open("Video.avi", fourcc, 14, (1920, 1374), False)   

while camera.IsGrabbing():
    grabResult = camera.RetrieveResult(5000, pylon.TimeoutHandling_ThrowException)   

    if grabResult.GrabSucceeded():
        # Access the image data
        image = converter.Convert(grabResult)
        img = image.GetArray()
        img = cv2.resize(img, (512, 512))      
        img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        
        fft = np.fft.fft2(img)
        fftShift = np.fft.fftshift(fft)
        magnitude =  0.1 * np.log(np.abs(fftShift))
        
        cv2.namedWindow('title', cv2.WINDOW_NORMAL)
        cv2.imshow('title', magnitude)

        # videoWriter.write(magnitude)

        k = cv2.waitKey(1)           

        if k == 27:
            break
    grabResult.Release()   
    
# Releasing the resource    
camera.StopGrabbing()
# videoWriter.release()
cv2.destroyAllWindows()
