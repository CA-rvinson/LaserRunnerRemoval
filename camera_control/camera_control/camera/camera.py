from abc import ABC, abstractmethod


class Camera(ABC):
    @abstractmethod
    def initialize(self):
        """Initialize any resources that have to be created on camera startup."""
        pass

    @abstractmethod
    def set_exposure(self, exposure_ms):
        """Set camera exposure time.

        Args:
            exposure_ms (float): exposure time in milliseconds. A negative number indicates auto-exposure.
        """
        pass

    @abstractmethod
    def get_frames(self):
        """Return a frame dict from the camera with a color and depth frame.

        Ret:
            {"color": Frame, "depth":Frame}: frame dictionary with a color and depth frame.
        """

        pass

    @abstractmethod
    def get_pos_location(self, x, y, frame):
        """Return the 3D positions with respect to the camera given a pixel location in the color frame.

        Args:
            x (int): x position of the pixel in the color frame.
            y (int): y position of the pixel in the color frame.
            frame ({"color": Frame, "depth": Frame}): frame dictionary with a color and depth frame.

        Ret:
            float[3]: array with float value for x y z position values.
        """
        pass
