// halftoned ball pattern for Panelix chain/combo markers

#version 3.7

global_settings
{
    
}

camera
{
    orthographic
    location -1*z
    look_at 0
    right 2*x
    up 2*y
}

plane
{
    z, 0
    texture
    {
        pigment
        {
            function
            {
                min(max(
                
                    1.25 - pow(x*x + y*y, 1/4) + 0.05*cos(16*pi*x) + 0.05*cos(16*pi*y)
                    
                , 0), 1)
            }
            
            rotate -30*z
            
            colour_map
            {
                [0.5 rgb 0]
                [0.5 rgb 1]
            }
        }
        finish
        {
            diffuse 0 ambient 1
        }
    }
}
