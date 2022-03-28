# Marching Cubes
I implemented marching cubes a while back for a course on VR,this is the implementation, straight from the original paper on the technique: https://people.eecs.berkeley.edu/~jrs/meshpapers/LorensenCline.pdf .


## Challenges
Easily the biggest challenge was generating the lookup table for all 256 possible cases. There is even a bug in the original paper that causes wholes to show up in the mesh, but I googled around and found the fix for that.

# alternate isosurface extraction using surface nets
There is also a "surface nets" implementation in here that was much simpler than marching cubes, it requires no 
lookup table but my implmentation was much slower than i expected,which is probably my fault. If i ever need to do 
isosurface extraction on a gpu i think Marching cubes is probably better because once, you have the table everything is easy. 