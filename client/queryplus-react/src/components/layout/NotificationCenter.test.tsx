import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { dismissNotification, getNotifications, notify } from "@/lib/notifications";
import { NotificationCenter } from "./NotificationCenter";

afterEach(() => {
    for (const notification of getNotifications()) dismissNotification(notification.id);
});

test("renders nothing when there are no notifications", () => {
    const { container } = render(<NotificationCenter />);

    expect(container).toBeEmptyDOMElement();
});

test("renders an active notification and dismisses it on close click", async () => {
    const user = userEvent.setup();
    notify("Can't reach the server. Check your connection and try again.");

    render(<NotificationCenter />);

    expect(
        screen.getByText("Can't reach the server. Check your connection and try again."),
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Dismiss" }));

    expect(getNotifications()).toHaveLength(0);
});
