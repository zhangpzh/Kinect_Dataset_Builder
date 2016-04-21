function RegistrateRgbAndDepth(viewName, viewPth)
% scaleRate_RgbToDepth;
% depthImageCroppedRowRange;
% colorImageCroppedColumnRange;
%缩放和平移的参数
if strcmp(viewName,'view-peizhen')
    scaleRate_RgbToDepth = 2.88000001;
    depthImageCroppedRowRange = [26+1,399+1];
    colorImageCroppedColumnRange = [85+1,596+1];
elseif strcmp(viewName,'view-yongyi')
    scaleRate_RgbToDepth = 2.8856393571727597;
    depthImageCroppedRowRange = [21+1,394+1];
    colorImageCroppedColumnRange = [97+1,608+1];
elseif strcmp(viewName, 'view-weihong')
    scaleRate_RgbToDepth = 2.882497801337617;
    depthImageCroppedRowRange = [32+1,405+1];
    colorImageCroppedColumnRange = [71+1,582+1];
end

%对于view中的所有video
for i = 1:length(dir(fullfile(viewPth)))
    videoName=strcat('video',num2str(i,'%02d'));
    colorImageDirectPth = fullfile(viewPth,videoName,'ColorImage');
    depthImageDirectPth = fullfile(viewPth,videoName,'DepthImage');
    %获取ColorImage 目录下所有 rgb 图片的名称
    colorFiles = dir(fullfile(colorImageDirectPth,'*.jpg'));
    %获取DepthImage 目录下所有 depth 图片的名称
    depthFiles = dir(fullfile(depthImageDirectPth,'*.png'));
    
    %裁剪 rgb 图片
    for j = 1:length(colorFiles)
        filePth = fullfile(colorImageDirectPth,colorFiles(j).name);
        image = imread(filePth);
        %rgb 图片 resize
        image=imresize(image,floor([1080.0/scaleRate_RgbToDepth 1920.0/scaleRate_RgbToDepth]));
        image=image(:,colorImageCroppedColumnRange(1):colorImageCroppedColumnRange(2),:);
        %将裁剪后的图片覆盖原图片
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
end