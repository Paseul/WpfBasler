'''
A simple Program for grabing video from basler camera and converting it to opencv img.
Tested on Basler acA1300-200uc (USB3, linux 64bit , python 3.5)

'''
from pypylon import pylon
import numpy as np
import cupy as cp
from matplotlib import pyplot as plt
import cv2
import skimage as sk

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
        img = cv2.resize(img, (1920, 1374))        
        img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        
        dft = cv2.dft(cp.float32(img),flags = cv2.DFT_COMPLEX_OUTPUT)
        dft_shift = cp.fft.fftshift(dft)
        
        magnitude_spectrum = 20*cp.log(cp.array(cv2.magnitude(cp.asnumpy(dft_shift[:,:,0]),cp.asnumpy(dft_shift[:,:,1]))))

        rows, cols = img.shape
        crow,ccol = int(rows/2), int(cols/2)        
        
        mask = cp.zeros((rows,cols,2),cp.uint8)      # create a mask first, center square is 1, remaining all zeros
        mask[crow-30:crow+30, ccol-30:ccol+30] = 1
        
        fshift = dft_shift*mask            # apply mask and inverse DFT
        f_ishift = cp.fft.ifftshift(fshift)        
        
        fshift = dft_shift*mask               # apply mask and inverse DFT
        f_ishift = cp.fft.ifftshift(fshift)
        img_back = cv2.idft(cp.asnumpy(f_ishift))
        img_back = cv2.magnitude(img_back[:,:,0],img_back[:,:,1])        
        
        img_back = cp.array(cp.array(img_back),dtype='float32')/float(2**32-1)
        img_back = cp.asnumpy(img_back)

        minVal = np.amin(img_back)
        maxVal = np.amax(img_back)
        img_back = cv2.convertScaleAbs(img_back, alpha=255.0/(maxVal - minVal), beta=-minVal * 255.0/(maxVal - minVal))
        
        cv2.namedWindow('title', cv2.WINDOW_NORMAL)
        cv2.imshow('title', img_back)

        videoWriter.write(img_back)

        k = cv2.waitKey(1)           

        if k == 27:
            break
    grabResult.Release()   
    
# Releasing the resource    
camera.StopGrabbing()
videoWriter.release()
cv2.destroyAllWindows()
