#Kinect\_Dataset\_Builder
>Kinect\_Dataset\_Builder is a repository containing a series of programs for constructing a Kinect video dataset or Kinect multi-view video dataset with Kinect sensor 2.0. The term “multi-view” means setting several sensors around a scene where activities or any other things else happen. Figure 1 shows three RGB images belonging to three views in a Kinect multi-view dataset at a moment.

![Figure-1](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-1.png)
<div  align="center">  Figure-1 </div>

### Building a single-view dataset：
>To build a single-view Kinect dataset, you first need to record the images of kinds of sources (RGB, depth, infrared, etc.) provided by a Microsoft Kinect sensor 2.0. Such program is a Kinect Recorder. Current repository provides such a recorder called “FroggyNect”. Now, suppose you have collected kinds of sources of images with “FroggyNect”. As we all know, the default resolution of RGB image s(1920x1080) is different from that of Depth’s, Infrared’s, and Body Index’s (512x424). Fortunately, this repository provides a program called “ImagesRegistrater” to conduct image registrations. (If you don’t know what exactly an image registration is, just Google it!)  Okay! Now, suppose a registrated dataset is available. Go on! Often, it’s necessary to acquire bounding boxes of people show up in each image within the videos of the dataset due to the demands of the training and test procedures in some computer vision problems such as group activity recognition. It’s lucky that this repository provides a program called “BoundingBoxer” to handle such thing. Till now, there is a dataset with annotations (bounding boxes in text files). However, it is not going to finish! I also wrote a program to transfer the text files with bounding boxes into .mat files defined by Matlab. This is because I am doing research on activity recognition problems in computer vision and I have found that researchers of this field usually like to organize the annotations of their newly-built dataset as mat files. Perhaps it’s difficult for them to write codes outside Matlab. Just a joke! Never mind! The program is named “AnnotationProducer”.

### Building a multi-view dataset：
>If what you want is a multi-view one, for instance, three views, you’ve got to set three Kinect sensors at three different views surrounding the scene then start recording. But soon you will find that it’s nearly impossible for these three sensors to record three videos respectively with the same number of images, although these recorders may have promised to trigger at the same moment. For example, in three “video01”s recorded by three Kinect sensors, there is always a “video01” containing more images than the other two. It seems like all we can do is to select the beginning frame number and ending frame number of these videos with respect by our naked eyes and organize these information as input of a sampling program to make synchronization. It’s not the worst case that I do provide a program named “MultiViewAligner” to handle this. So feel proud of me! Then, similarly we can apply “ImagesRegistrater”, “BoundingBoxer” and “AnnotationProducer” to every view just like what we have done upon a single-view one as described in the last paragraph.

### Brief introduction to the five programs:
Now, please allow me to give a brief introduction to these five handsome software.
> * FroggyNect
> * MultiViewAligner
> * ImagesRegistrater
> * BoundingBoxer
> * AnnotationProducer

---

#### 1.	FroggyNect 
FroggyNect，the Kinect Recorder, is enable to reach the following goals:
>1.	Monitor: Fetch and show RGB, depth and skeleton images (skeleton images are drawn by using corresponding skeleton joints) from Kinect sensor in real time.
2.	Recorder: Store RGB, depth, infrared data as formatted images (jpg, png, and so on), skeleton data in text files in real time. (The skeleton text contains 25 skeleton joints’ coordinates in three spaces: camera, color and depth. It also contains the orientations of bones and a floor clip plane of that frame.)

You guys are sure to record data streams from Kinect sensor(s) meanwhile open the monitor to have a peek at what you are recording. The written fps of the images is displayed on the main UI of FroggyNect. It’s recommended that you use SSD and faster processor(s) in order to lose as little information as possible.
Figure 2 shows the user interface of FroggyNect. 
![Figure-2](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-2.png)
<div  align="center">  Figure-2 </div>
Figure 3 shows a screenshot of the application while recording and monitoring information from a Kinect sensor.
<div  align="center">  Figure-3 </div>

---

#### 2.	MultiViewAligner
If you have collected multiple views of videos by using several Kinect sensors with each connected with an instinct computer, you are supposed to use our MultiViewAligner to synchronize these views. All you have to do is to offer it a configuration text file. The contents of a sample configuration text file are listed in Figure 4:

![Figure-4](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-4.png)
<div  align="center">  Figure-4 </div>

There are original multi-view dataset directory and the new multi-view dataset directory presenting at the first line.
In the second line, there are view number (supposed n) and n views’ names.
The third line reveals that we want to synchronize “video01”s of three views (view_left, view_middle and view_right) in original dataset and form three new “video01”s with the same length in the new dataset. The sampling ranges of corresponding three “video01”s are 2-180, 50-250, 23-230 respectively. After synchronization, we’re going to find three “video01”s each with 179 images in their sub-directories of different kinds of images under the root directory of the new dataset.

---

#### 3.	ImagesRegistrater
This module contains exactly two sub-programs:  “GetRegisParams” and “RegisProgs”. First, I shall explain what exactly registrations are conducted on our images. If you have already learn the structure of the Kinect sensor, you may know that the perspective of the RGB sensor is different from that of the infrared sensor. The production of the RGB images is relevant to the RGB sensor while the production of other kinds of images is relevant to the infrared sensor. This results in the difference of the angle of views of RGB images and other kinds of images (depth, infrared, long infrared and body index images).  Actually, the RGB images own wider perspective than others in horizontal space while other kinds of images own wider perspective than RGB images in vertical space. What’s more, there is a zooming relation between RGB images and other images. Our programs solve these problems by finding the scale ratio and cropped ranges (crop some columns of RGB images and some rows of other kinds of images). 

We use “GetRegisParams” to acquire the scale ratio of RGB to depth images and cropped ranges. Supplying several pairs of skeleton records to our program did this. Each pair of skeleton record contains 25 pairs of depth skeleton joints’ coordinates and 25 pairs of color (RGB) skeleton joints’ coordinates and belongs to a specific person in a specific frame. (We can get these data easily from the SkeletonInfo.txt inside the dataset) It’s recommended that you compute scale ratio and cropped ranges for each view of your dataset respectively. That’s is to say, if you want to estimate the parameters for a view, you have to provide our program with several pairs of skeleton records belonging skeletons show up in that view’s videos. Actually, the skeleton records are easily to be found in the ‘SkeletonInfo\SkeletonInfo.txt’. Just copy, paste, and eliminate the headers (“color_skeleton_coordinates =” and “depth_skeleton_coordinates=”). 

Our experiments demonstrate that these parameters differ from views and we’d better calculate each combination of parameters for each view. Besides, it’s a good way to input as many pairs of skeleton records within the same view as possible to improve the accuracy upon the estimation of the parameters of a view. This data we need is listed in Figure 5.

![Figure-5](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-5.png)
<div  align="center">  Figure-5 </div>

The records in Figure 5 are from a “SkeletonInfo.txt” under a video of a view.
After that, we get a text containing parameters like what is displaying in Figure 6.

![Figure-6](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-6.png)
<div  align="center">  Figure-6 </div>

Then start our program “RegisProgs”, and it will read the text and registrate images. But before that you have to modify the paths (the absolute paths of the dataset and the parameter text) in the source codes manually. 
“RegisProgs” is made up of two .m files. The main function is in “croppedImages.m”.

---

#### 4. BoundingBoxer
You can draw bounding boxes of people who appear in the RGB images in a selected video with the help of BoundingBoxer. Start the program, and you will see six buttons on the window.

![Figure-7](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-7.png)
<div  align="center">  Figure-7 </div>

Click “Browse” button when you want to select a video to draw bounding boxes. In Figure 8, we select video95. Press enter key then we get Figure 9.

![Figure-8](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-8.png)
<div  align="center">  Figure-8 </div>

![Figure-9](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-9.png)
<div  align="center">  Figure-9 </div>

Before we start drawing bounding boxes, we have to know that BoundingBoxer will copy the bounding boxes of current image to the next one. And after clicking “Next” button, the program will jump to the next image of the next image of current image. That is to say, the incrementing number is 2. This is because Kinect sensor can receive at most 30 frames a second, which causes the situation that too many images are waiting to be annotated, and usually the tiny difference between two consecutive images can be ignored. However, if you are patient enough to draw bounding boxes for each frame, you can easily modify the source codes.

Click the Append button, then we enter the append mode. In this mode, when you click the canvas twice, a rectangle (bounding box) will be drawn according to the two points captured by our program. You will be asked to input the person id as soon as the rectangle is drawn. And after a valid person id is given, a bounding box will display with the person’s name and id in case of our carelessness. You are free to draw whatever number of bounding boxes you want just like figure 10 shows until you click the “Cancel” button.

![Figure-10](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-10.png)
<div  align="center">  Figure-10 </div>

If you are not satisfied with your boxes and want to eliminate some of them, you just need to click the “Cancel” button then the “Delete” button to enter the delete mode. Then just move your mouse inside a rectangle off which you want to get rid. The borders of the boxes containing the mouse will turn their color into red. Simply left click the mouse, then the boxes with red borders will disappear. Figure 11 and 12 show an example.

![Figure-11](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-11.png)
<div  align="center">  Figure-11 </div>

![Figure-12](https://github.com/zhangpzh/Kinect_Dataset_Builder/raw/master/mdImages/Figure-12.png)
<div  align="center">  Figure-12 </div>

If you have finished annotating current frame (also the next one’s too), you can click “Next” button to enter the next image with which you are going to work. Any time, you are free to click “Previous” button to check the previous images up, regret and modify them.
Be relaxed to click the exit button at the top right corner when you feel tired of this boring job. The program will automatically preserve your working process. Next time you open the application and browse the video folder, you are going to continue your work. One thing to be noted that you won’t be able to modify the images that you have viewed last time after you close the program and open it again because the “Previous” button is constrained to avoid such operations. The most previous image you can reach with this button is written into the working process file. Of course, you can easily modify the source codes to make it possible to modify any image at any time whenever you start BoundingBoxer.

---

#### 5. AnnotationProducer
“AnnotationProducer” is a simple Matlab script to transfer the bounding boxes text files into .mat file.

---

#### The source codes of these five programs as aforementioned are available in this repository. Make sure you have starred it before you use it! Have fun, guys!
