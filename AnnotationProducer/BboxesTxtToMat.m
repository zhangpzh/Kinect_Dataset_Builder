viewNames= {'view01','view02','view03'};
basicPth = 'I:\kinect-dataset(multi-view for activity)';
b=size(viewNames);
totalPersonIdCnt = 10;
videoNumberOfEachView = 95;

% video labels:
% 0->talking
% 1->fighting
% 2->following
% 3->waiting in line
% 4->entering
% 5->gathering
% 6->dismissing

% multi-view 每个 view 的 相同 video 号的，都是一段视频的不同角度, 因此label相同
% 每个view都有 95 个video，因此有 95 个不同的 label. 用数组 labels 来存储

% video01~18 -> talking
labels = zeros(1,18);
% video19 -> fighting
labels = [labels,1];
% video20~35 -> following
tmp = zeros(1,16);
tmp = tmp+2;
labels = [labels,tmp];
% video36~39 -> waiting in line
tmp = zeros(1,4);
tmp = tmp+3;
labels = [labels,tmp];
% video40~47 -> following
tmp = zeros(1,8);
tmp = tmp+2;
labels = [labels,tmp];
% video48~51 -> fighting
tmp = zeros(1,4);
tmp = tmp+1;
labels = [labels,tmp];
% video52~57 -> waiting in line
tmp = zeros(1,6);
tmp = tmp+3;
labels = [labels,tmp];
% video58~62 -> fighting only
tmp = zeros(1,5);
tmp = tmp+1;
labels = [labels,tmp];
% video63~75 -> entering
tmp = zeros(1,13);
tmp = tmp + 4;
labels = [labels,tmp];
% video76~95 -> gathering and dismissing in turn
tmp = [5 6 5 6 5 6 5 6 5 6 5 6 5 6 5 6 5 6 5 6];
labels = [labels,tmp];

for i = 1:b(2)
    viewPth = fullfile(basicPth,viewNames{i});
    %对于所有的view的每一个video中的BBoxes.txt
    for j = 1:videoNumberOfEachView
        videoName=strcat('video',num2str(j,'%02d'));
        BboxesTxtPth = fullfile(viewPth,videoName,'BBoxes.txt');
        showUpList = [0 0 0 0 0 0 0 0 0 0];
        %读取BBoxes.txt
        fhandle = fopen(BboxesTxtPth);
        contents = textscan(fhandle,'%u%u%u%u%u%u');
        frameNumber_vector = contents{1};
        personId_vector = contents{2};
        topLeftPointX_vector = contents{3};
        topLeftPointY_vector = contents{4};
        BBoxesWidth_vector = contents{5};
        BBoxesHeight_vector = contents{6};
        n_Records = length(frameNumber_vector);
        %赋予当前video的label
        anno.label = labels(j);
        %获得当前video中帧的总数
        anno.n_Frame = frameNumber_vector(n_Records);
        %对于BBoxes.txt中的每一条记录 (对应于一个 bounding boxes)
        %扫一遍知道有哪些人存在
        for k = 1:n_Records
            frameNumber = frameNumber_vector(k);
            personId = personId_vector(k);
            topLeftPointX = topLeftPointX_vector(k);
            topLeftPointY = topLeftPointY_vector(k);
            BBoxesWidth = BBoxesWidth_vector(k);
            BBoxesHeight = BBoxesHeight_vector(k);
            %做标记
            if showUpList(personId) == 0
                showUpList(personId) = 1;
            end
        end
        anno.n_ShowUp = sum(showUpList);
        %创建一个长度为 n_ShowUp 的 cell, 并建立映射数组
        anno.people  = cell(1,anno.n_ShowUp);
        mappingList = cell(1,anno.n_ShowUp);
        %映射计数器
        counter = 1;
        for k = 1:totalPersonIdCnt
            if showUpList(k) ~= 0
                mappingList{k} = counter;
                % 填充id
                anno.people{counter}.id = k;
                counter = counter + 1;
            end
        end
        %初始化 bounding boxes 的结构
        for k = 1:anno.n_ShowUp
            anno.people{k}.time = [];
            anno.people{k}.bbs = [];
        end
        
        %填充 bounding boxes 记录
        for k = 1:n_Records
            frameNumber = frameNumber_vector(k);
            personId = personId_vector(k);
            topLeftPointX = topLeftPointX_vector(k);
            topLeftPointY = topLeftPointY_vector(k);
            BBoxesWidth = BBoxesWidth_vector(k);
            BBoxesHeight = BBoxesHeight_vector(k);
            %获得在people中的索引
            indexInPeople = mappingList{personId};
            anno.people{indexInPeople}.time = [anno.people{indexInPeople}.time frameNumber];
            box = [topLeftPointX topLeftPointY BBoxesWidth BBoxesHeight];
            anno.people{indexInPeople}.bbs = [anno.people{indexInPeople}.bbs;box];
        end
        
        %保存为 .mat 文件到对应 video 的文件夹中
        fileName = strcat('view',num2str(i,'%02d'),'_','video',num2str(j,'%02d'),'.mat');
        savePth = fullfile(basicPth,'annotations',fileName);
        save(savePth,'anno');
    end
end