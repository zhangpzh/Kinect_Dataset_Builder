viewNames= {'view-peizhen','view-yongyi','view-weihong'};
%basicPth = 'C:\Users\Zhang\Desktop\fake';
basicPth = 'I:\kinect-dataset(multi-view for activity)';
b=size(viewNames);
for i = 1:b(2)
    RegistrateRgbAndDepth(viewNames{i},fullfile(basicPth,viewNames{i}));
end