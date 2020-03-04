# Basler Camera with python
## 파일 소개
1. Basler.py
    Basler USB 카메라를 구동시킴(가우시안 분포를 찾아내서 그 분포를 타원으로 표시)

2. Basler_nano.py
    Basler.py의 Jetson nano에서 구동시킬수 있는 파일

3. Basler_fourier.py
    Basler 카메라에 퓨리에 변환을 적용

4. video_tracking.py
    주어진 동영상에서 가우시안 분포를 찾아냄

5. Iso11146.py
    가우시안 분포를 찾아내는 클래스로서 ISO11146를 적용하였다



## 설치 및 실행 방법
1. pylon 설치
[리눅스 64비트 설치](https://www.baslerweb.com/ko/sales-support/downloads/software-downloads/pylon-5-2-0-linux-x86-64-bit-debian/)
[리눅스 32비트 설치](https://www.baslerweb.com/ko/sales-support/downloads/software-downloads/pylon-5-2-0-linux-x86-32-bit-debian/)
[윈도우 64비트 설치](https://www.baslerweb.com/ko/sales-support/downloads/software-downloads/pylon-6-0-1-windows/)

/opt/pylon5/README.MD 파일을 참조하여 환경 변수 설정

2. Swig 설치
$ Sudo apt install swig

3. pylon pip 설치
& pip install pypylon

4. 실행
$ python Basler.py --e 20000 --o video/output.avi --min 50 --max 255
$ python Basler_nano.py
$ python basler_fourier.py
$ python video_tracking.py --v video/cropped.avi --o video/tracked.avi --min 50 --max 255
$ python video_cropping.py --v video/4_origin.avi --o video/cropped.avi --min 50 --max 255