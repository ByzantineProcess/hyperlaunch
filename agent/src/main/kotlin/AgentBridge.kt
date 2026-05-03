package dev.byzann.hyperlaunch.agent

import java.util.concurrent.CountDownLatch


object AgentBridge {
    private val GATE = CountDownLatch(1)

    fun onWindowCreationPoint(interceptedClass: String) {
        try {
            if (interceptedClass == GLFW_CLASS) {
                setGlfwHidden()
            }
        } catch (e: InterruptedException) {
            Thread.currentThread().interrupt()
        }
    }

    fun proceed() {
        GATE.countDown()
    }

    @Throws(Exception::class)
    private fun setGlfwHidden() {
        val glfw = Class.forName("org.lwjgl.glfw.GLFW")
        glfw.getMethod("glfwWindowHint", Int::class.javaPrimitiveType, Int::class.javaPrimitiveType)
            .invoke(null, 0x00020004,  /*GLFW_VISIBLE*/0 /*GLFW_FALSE*/)
    }
}
