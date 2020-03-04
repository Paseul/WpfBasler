import cv2
import numpy as np

class Iso11146:
    def ellipse(img, min_thresh = 50, max_threash = 255):
        _, thresh = cv2.threshold(img, min_thresh, max_threash, 0)

        contours, _ = cv2.findContours(thresh,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
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
                cv2.circle(img, (cX, cY), 1, (255, 255, 255), -1)
                s = str(cX) + "." +str(cY)
                cv2.putText(img, s, (cX, cY), cv2.FONT_HERSHEY_SIMPLEX, 1, (255,255,255), 2)
                cv2.ellipse(img, (cX, cY), (dX, dY), theta, 0, 360, (255, 255, 255), 2)

        return img

    def crop_center(img, min_thresh = 50, max_threash = 255):
        _, thresh = cv2.threshold(img, min_thresh, max_threash, 0)

        contours, _ = cv2.findContours(thresh,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
        for c in contours:
            # discard small contours
            if (cv2.contourArea(c) < 1000):
                continue            

            # calculate moments
            M = cv2.moments(c)
            
            if (M["m00"]!= 0):
                cX = int(M["m10"] / M["m00"])
                cY = int(M["m01"] / M["m00"])                

                return cX, cY
