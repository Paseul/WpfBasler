# import the necessary packages
from collections import deque
from imutils.video import VideoStream
from tkinter import messagebox
from Iso11146 import Iso11146
import numpy as np
import sys
import argparse
import cv2
import imutils
import time
# construct the argument parse and parse the arguments
ap = argparse.ArgumentParser()
ap.add_argument("-v", "--video",
	help="path to the (optional) video file")
ap.add_argument("-o", "--output",
	help="path to the (optional) video file")
ap.add_argument("-min", "--minimum_thresh", type=int, default=50,
	help="minimum thresh value")
ap.add_argument("-max", "--maximum_thresh", type=int, default=255,
	help="maximum thresh value")
args = vars(ap.parse_args())

# if a video path was not supplied, grab the reference
# to the webcam
if not args.get("video", False):
	messagebox.showinfo(title="video file path error", message="-v, --video path to the (optional) video file")
	sys.exit()
# otherwise, grab a reference to the video file
else:
	vs = cv2.VideoCapture(args["video"])
# allow the camera or video file to warm up
time.sleep(2.0)

# save video
videoWriter = cv2.VideoWriter()

fourcc = cv2.VideoWriter_fourcc(*'XVID')
videoWriter.open(args["output"], fourcc, 14, (3840, 2748), False)   

# keep looping
while True:
	# grab the current frame
	frame = vs.read()
	# handle the frame from VideoCapture or VideoStream
	frame = frame[1]
	# if we are viewing a video and we did not grab a frame,
	# then we have reached the end of the video
	if frame is None:
		break

	img = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

	np.where(img < 5, 0, img)  

	img = Iso11146.ellipse(img, args["minimum_thresh"], args["maximum_thresh"])

	cv2.namedWindow('title', cv2.WINDOW_NORMAL)
	cv2.imshow('title', img)

	videoWriter.write(img)
	
	k = cv2.waitKey(1) 
	# time.sleep(0.1)          	
	
	if k == 27:
		break

vs.release()
videoWriter.release()
# close all windows
cv2.destroyAllWindows()
