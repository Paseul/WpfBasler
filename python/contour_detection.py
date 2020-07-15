# import the necessary packages
from collections import deque
from imutils.video import VideoStream
from tkinter import messagebox
import os
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
ap.add_argument("-min", "--minimum_thresh", type=int, default=122,
	help="minimum thresh value")
ap.add_argument("-max", "--maximum_thresh", type=int, default=255,
	help="maximum thresh value")
args = vars(ap.parse_args())

# if a video path was not supplied, grab the reference
# to the webcam
# if not args.get("video", False):
# 	messagebox.showinfo(title="video file path error", message="-v, --video path to the (optional) video file")
# 	sys.exit()
# otherwise, grab a reference to the video file
# save video
videoWriter = cv2.VideoWriter()

file_list = os.listdir(args["video"])
for item in file_list:
	print(item)
	vs = cv2.VideoCapture(args["video"] + "/" + item)
	# allow the camera or video file to warm up
	time.sleep(2.0)	

	fourcc = cv2.VideoWriter_fourcc(*'XVID')
	output = args["video"] + "/" + os.path.splitext(item)[0] + "_out.avi"
	videoWriter.open(output , fourcc, 90, (2048, 2048), False)

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
		_, thresh = cv2.threshold(img, args["minimum_thresh"], args["maximum_thresh"], 0)
		contours, _ = cv2.findContours(thresh,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
		for c in contours:
			square = cv2.contourArea(c)
			if (square > 500):
				text = "{}".format(square)
			else:
				continue
				
			# calculate moments
			M = cv2.moments(c)

			if (M["m00"]!= 0):
				cX = int(M["m10"] / M["m00"])
				cY = int(M["m01"] / M["m00"])

			cv2.putText(img, text, (cX, cY), cv2.FONT_HERSHEY_SIMPLEX, 1, (0,0,0), 2)

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
