viewNames= {'view-peizhen','view-yongyi','view-weihong'};
basicPth = 'I:\kinect-dataset(multi-view for activity)';
b=size(viewNames);
for i = 1:b(2)
    RegistrateImages(fullfile(basicPth,viewNames{i}));
end