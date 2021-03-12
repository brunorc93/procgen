This document states the rules used in creating the RGBA maps for each unique construct:

R: determines construct properties
	if R == 255: is construct;
	if R == 100 || 120: entry point,
		where 100 is further from the construct so to create a direction a procedural path must come from;

G: determines foundation properties
	if G > 0: is foundation (must follow delta height rules);
		G == 100: fully flat
		G == 110: flat outline

		G == 150: delta-h is upwards
		G == 160: upwards d-h outline
		
		G == 200: delta-h is downwards
		G == 210: downwards d-h outline

B: determines delta-heightmap values
	B value equals the difference in height to any B = 0 point

A: shows if there is info in the pixel to be read in the RGB values
	if A > 0: there is info
		A == 255: has to be built in the island
		A < 200 (70% oppacity is ok) : has to be built in THE VOID (shape_bmp.color.a == 0)
    A > 200 && < 255 (80% oppacity is ok): can be built in either island or void