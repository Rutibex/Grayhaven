### Overview
This tutorial demonstrates how a stylesheet could be edited using the MyraPad.

### Tutorial.
1. Download and unpack file: [stylesheetEdit.zip](~/files/stylesheetEdit.zip). 

   It consists from UI in MML(allControls.xmmp) and linked stylesheet(which is copy of Myra default stylesheet).
2. Open allControls.xmmp in the [MyraPad](MyraPad.md):

![alt text](~/images/editing-stylesheet-using-myrapad1.png)

3. Open stylesheet/default_ui_skin.xmms in any text editor.
4. Now let's make button background - lightcoral. 

   Find following line:
   ```xml
     <ButtonStyle Background="button" OverBackground="button-over" PressedBackground="button-down" />
   ```
   And replace it with:   
   ```xml
     <ButtonStyle Background="#F08080" OverBackground="button-over" PressedBackground="button-down" />
   ```
   Save stylesheet/default_ui_skin.xml.
5. Now, in the MyraPad, click File/Reload or press Ctrl+R.

   Observe - button backgrounds have become lightcoral:

![alt text](~/images/editing-stylesheet-using-myrapad2.png)

