function RegistrateImages(rootPthOfVideos)
% Reading parameters from 'rootPthOfVideos\registerParams.txt'
regisPrmTxtPth = fullfile(rootPthOfVideos,'registerParams.txt');
fid = fopen(regisPrmTxtPth);
tline = fgetl(fid);
tline = str2num(tline);
scaleRate_RgbToDepth = tline(7);
% +1 是因为 matlab 中的 index 都是从 1 算起的
depthImageCroppedRowRange = [tline(3)+1 tline(4)+1];
colorImageCroppedColumnRange = [tline(1)+1 tline(2)+1];

% 常量, Kinect 2 代默认收集的rgb帧的大小: 1920 x 1080
origColorWidth = 1920.0;
origColorHeight = 1080.0;

%对于当前view中的所有video
for i = 1:length(dir(fullfile(rootPthOfVideos)))
    videoName=strcat('video',num2str(i,'%02d'));
    colorImageDirectPth = fullfile(rootPthOfVideos,videoName,'ColorImage');
    depthImageDirectPth = fullfile(rootPthOfVideos,videoName,'DepthImage');
    bodyIndexImageDirectPth = fullfile(rootPthOfVideos,videoName,'BodyIndexImage');
    infraredImageDirectPth = fullfile(rootPthOfVideos,videoName,'InfraredImage');
    %获取ColorImage 目录下所有 rgb 图片的名称
    colorFiles = dir(fullfile(colorImageDirectPth,'*.jpg'));
    %获取DepthImage 目录下所有 depth 图片的名称
    depthFiles = dir(fullfile(depthImageDirectPth,'*.png'));
    %获取BodyIndexImage 目录下所有 body index 图片的名称
    bodyIndexFiles = dir(fullfile(bodyIndexImageDirectPth,'*.jpg'));
    %获取InfraredImage 目录下所有 infrared 图片的名称
    infraredFiles = dir(fullfile(infraredImageDirectPth,'*.jpg'));
    
    %裁剪 rgb 图片
    for j = 1:length(colorFiles)
        filePth = fullfile(colorImageDirectPth,colorFiles(j).name);
        image = imread(filePth);
        %rgb 图片 resize
        image=imresize(image,floor([origColorHeight/scaleRate_RgbToDepth origColorWidth/scaleRate_RgbToDepth]));
        image=image(:,colorImageCroppedColumnRange(1):colorImageCroppedColumnRange(2),:);
        %%将裁剪后的图片覆盖原图片
        imwrite(image,filePth);
    end
    %裁剪 depth 图片
    for j = 1:length(depthFiles)
        filePth = fullfile(depthImageDirectPth,depthFiles(j).name);
        image = imread(filePth);
        image=image(depthImageCroppedRowRange(1):depthImageCroppedRowRange(2),:);
        %将裁剪后的图片覆盖原图片
        imwrite(image,filePth);
    end
    %裁剪 body index 图片
    for j = 1:length(bodyIndexFiles)
        filePth = fullfile(bodyIndexImageDirectPth,bodyIndexFiles(j).name);
        image = imread(filePth);
        %bodyIndexImage的视角和 depth 的一样, 所以和 depth image 的裁剪范围相同
        image=image(depthImageCroppedRowRange(1):depthImageCroppedRowRange(2),:);
        %将裁剪后的图片覆盖原图片
        imwrite(image,filePth);
    end
    %裁剪 infrared 图片
    for j = 1:length(infraredFiles)
        filePth = fullfile(infraredImageDirectPth,infraredFiles(j).name);
        image = imread(filePth);
        %infraredImage的视角和 depth 的一样, 所以和 depth image 的裁剪范围相同
        image=image(depthImageCroppedRowRange(1):depthImageCroppedRowRange(2),:);
        %将裁剪后的图片覆盖原图片
        imwrite(image,filePth);
    end
end