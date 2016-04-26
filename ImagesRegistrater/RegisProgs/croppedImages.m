%------------------------------------------------------------------------------
% <copyright file="croppedImages.m" author="Peizhen Zhang" email="peizhenzhang73@gmail.com">
%     Copyright (c) Peizhen Zhang.  All rights reserved.
% </copyright>
%------------------------------------------------------------------------------
viewNames= {'view-peizhen','view-yongyi','view-weihong'};
basicPth = 'I:\kinect-dataset(multi-view for activity)';
b=size(viewNames);
for i = 1:b(2)
    RegistrateImages(fullfile(basicPth,viewNames{i}));
end