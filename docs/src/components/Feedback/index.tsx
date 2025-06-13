import React from "react";
import "./index.css";

export default function Feedback() {
  return (
    <div className="feedbackContainer feedbackLink">
      <span role="img" aria-label="idea">
        💡
      </span>
      Was this page helpful?&nbsp;
      <a
        href="https://github.com/aevatarAI/aevatar-documentation/issues/new"
        target="_blank">
        Click here to give feedback!
      </a>
    </div>
  );
}
